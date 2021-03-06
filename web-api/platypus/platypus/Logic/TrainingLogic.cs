﻿using Nssol.Platypus.DataAccess.Core;
using Nssol.Platypus.DataAccess.Repositories.Interfaces.TenantRepositories;
using Nssol.Platypus.Infrastructure;
using Nssol.Platypus.Infrastructure.Types;
using Nssol.Platypus.Logic.Interfaces;
using Nssol.Platypus.Models.TenantModels;
using System;
using System.Threading.Tasks;

namespace Nssol.Platypus.Logic
{
    public class TrainingLogic : PlatypusLogicBase, ITrainingLogic
    {
        private readonly ITrainingHistoryRepository trainingHistoryRepository;
        private readonly ITensorBoardContainerRepository tensorBoardContainerRepository;
        private readonly IClusterManagementLogic clusterManagementLogic;
        private readonly IUnitOfWork unitOfWork;

        public TrainingLogic(
            ITrainingHistoryRepository trainingHistoryRepository,
            ITensorBoardContainerRepository tensorBoardContainerRepository,
            IClusterManagementLogic clusterManagementLogic,
            IUnitOfWork unitOfWork,
            ICommonDiLogic commonDiLogic) : base(commonDiLogic)
        {
            this.trainingHistoryRepository = trainingHistoryRepository;
            this.tensorBoardContainerRepository = tensorBoardContainerRepository;
            this.clusterManagementLogic = clusterManagementLogic;
            this.unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 学習履歴コンテナを削除し、ステータスを変更する。
        /// </summary>
        /// <param name="trainingHistory">対象学習履歴</param>
        /// <param name="status">変更後のステータス</param>
        /// <param name="force">他テナントに対する変更を許可するか</param>
        public async Task ExitAsync(TrainingHistory trainingHistory, ContainerStatus status, bool force)
        {
            // コンテナの生存確認
            if (trainingHistory.GetStatus().Exist())
            {
                var info = await clusterManagementLogic.GetContainerDetailsInfoAsync(trainingHistory.Key, CurrentUserInfo.SelectedTenant.Name, force);

                // コンテナ削除の前に、DBの更新を先に実行
                await trainingHistoryRepository.UpdateStatusAsync(trainingHistory.Id, status, info.CreatedAt, DateTime.Now, force);

                // 実コンテナ削除の結果は確認せず、DBの更新を先に確定する（コンテナがいないなら、そのまま消しても問題ない想定）
                unitOfWork.Commit();

                if (info.Status.Exist())
                {
                    // 再確認してもまだ存在していたら、コンテナ削除
                    await clusterManagementLogic.DeleteContainerAsync(
                        ContainerType.Training, trainingHistory.Key, CurrentUserInfo.SelectedTenant.Name, force);
                }
            }
            else
            {
                await trainingHistoryRepository.UpdateStatusAsync(trainingHistory.Id, status, force);
                
                // DBの更新を確定する
                unitOfWork.Commit();
            }
        }

        /// <summary>
        /// TensorBoardコンテナを削除する。
        /// </summary>
        /// <param name="container">対象コンテナ</param>
        /// <param name="force">他テナントに対する変更を許可するか</param>
        public async Task DeleteTensorBoardAsync(TensorBoardContainer container, bool force)
        {
            //TensorBoardコンテナを削除する。
            await clusterManagementLogic.DeleteContainerAsync(ContainerType.TensorBoard, container.Name, CurrentUserInfo.SelectedTenant.Name, force);

            //結果に関わらず、DBからコンテナ情報を消す
            tensorBoardContainerRepository.Delete(container, force);

            unitOfWork.Commit();
        }
    }
}
